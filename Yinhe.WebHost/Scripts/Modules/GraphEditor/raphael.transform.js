(function(R) {

// Add Array.map if the browser doesn't support it
if ( !Array.prototype.hasOwnProperty('map') ) {
    Array.prototype.map = function(callback, arg) {
        var i, mapped = [];

        for ( i in this ) {
            if ( this.hasOwnProperty(i) ) { mapped[i] = callback.call(arg, this[i], i, this); }
        }

        return mapped;
    };
}

// Add Array.indexOf if not builtin
if ( !Array.prototype.hasOwnProperty('indexOf') ) {
    Array.prototype.indexOf = function(obj, start) {
        for ( var i = (start || 0), j = this.length; i < j; i++ ) {
            if ( this[i] === obj ) { return i; }
        }
        return -1;
    }
}

R.fn.freeTransform = function(host, subject, options, callback) {
	// Enable method chaining
	if ( subject.freeTransform ) { return subject.freeTransform; }

	var
		paper = this,
		bbox  = subject.getBBox(true)
		;

	var ft = subject.freeTransform = {
		// Keep track of transformations
		attrs: {
			x: bbox.x,
			y: bbox.y,
			size: { x: bbox.width, y: bbox.height },
			center: { x: bbox.x + bbox.width  / 2, y: bbox.y + bbox.height / 2 },
			rotate: 0,
			scale: { x: 1, y: 1 },
			translate: { x: 0, y: 0 },
			ratio: 1
			},
		axes: null,
		bbox: null,
		callback: null,
		items: [],
		handles: { center: null, x: null, y: null },
		offset: {
			rotate: 0,
			scale: { x: 1, y: 1 },
			translate: { x: 0, y: 0 }
			},
		opts: {
			animate: false,
			attrs: { fill: '#fff', stroke: '#000' },
			boundary: { x: paper._left || 0, y: paper._top || 0, width: paper.width, height: paper.height },
			distance: 1.3,
			drag: true,
			draw: false,
			keepRatio: false,
			range: { rotate: [ -180, 180 ], scale: [ -99999, 99999 ] },
			rotate: true,
			scale: true,
			snap: { rotate: 0, scale: 0, drag: 0 },
			snapDist: { rotate: 0, scale: 0, drag: 7 },
			size: 5
			},
        subject: subject,
        host: host,
        copyGhost: null
		};

	/**
	 * Update handles based on the element's transformations
	 */
	ft.updateHandles = function() {
		if ( ft.handles.bbox || ft.opts.rotate.indexOf('self') >= 0 ) {
			var corners = getBBox();
		}

		// Get the element's rotation
		var rad = {
			x: ( ft.attrs.rotate      ) * Math.PI / 180,
			y: ( ft.attrs.rotate + 90 ) * Math.PI / 180
			};

		var radius = {
			x: ft.attrs.size.x / 2 * ft.attrs.scale.x,
			y: ft.attrs.size.y / 2 * ft.attrs.scale.y
			};

		ft.axes.map(function(axis) {
			if ( ft.handles[axis] ) {
				var
					cx = ft.attrs.center.x + ft.attrs.translate.x + radius[axis] * ft.opts.distance * Math.cos(rad[axis]),
					cy = ft.attrs.center.y + ft.attrs.translate.y + radius[axis] * ft.opts.distance * Math.sin(rad[axis])
					;

				// Keep handle within boundaries
				if ( ft.opts.boundary ) {
					cx = Math.max(Math.min(cx, ft.opts.boundary.x + ft.opts.boundary.width),  ft.opts.boundary.x);
					cy = Math.max(Math.min(cy, ft.opts.boundary.y + ft.opts.boundary.height), ft.opts.boundary.y);
				}

				ft.handles[axis].disc.attr({ cx: cx, cy: cy });

				ft.handles[axis].line.toFront().attr({
					path: [ [ 'M', ft.attrs.center.x + ft.attrs.translate.x, ft.attrs.center.y + ft.attrs.translate.y ], [ 'L', ft.handles[axis].disc.attrs.cx, ft.handles[axis].disc.attrs.cy ] ]
					});

				ft.handles[axis].disc.toFront();
			}
		});

		if ( ft.bbox ) {
			ft.bbox.toFront().attr({
				path: [
					[ 'M', corners[0].x, corners[0].y ],
					[ 'L', corners[1].x, corners[1].y ],
					[ 'L', corners[2].x, corners[2].y ],
					[ 'L', corners[3].x, corners[3].y ],
					[ 'L', corners[0].x, corners[0].y ]
					]
				});

			// Allowed x, y scaling directions for bbox handles
			var bboxHandleDirection = [
				[ -1, -1 ], [ 1, -1 ], [ 1, 1 ], [ -1, 1 ],
				[  0, -1 ], [ 1,  0 ], [ 0, 1 ], [ -1, 0 ]
				];

			if ( ft.handles.bbox ) {
				ft.handles.bbox.map(function (handle, i) {
					var cx, cy, j, k;

					if ( handle.isCorner ) {
						cx = corners[i].x;
						cy = corners[i].y;
					} else {
						j  = i % 4;
						k  = ( j + 1 ) % corners.length;
						cx = ( corners[j].x + corners[k].x ) / 2;
						cy = ( corners[j].y + corners[k].y ) / 2;
					}

					handle.element.toFront()
						.attr({
							x: cx - ( handle.isCorner ? ft.opts.size.bboxCorners : ft.opts.size.bboxSides ),
							y: cy - ( handle.isCorner ? ft.opts.size.bboxCorners : ft.opts.size.bboxSides )
							})
						.transform('R' + ft.attrs.rotate)
						;

					handle.x = bboxHandleDirection[i][0];
					handle.y = bboxHandleDirection[i][1];
				});
			}
		}

		if ( ft.circle ) {
			ft.circle.attr({
				cx: ft.attrs.center.x + ft.attrs.translate.x,
				cy: ft.attrs.center.y + ft.attrs.translate.y,
				r:  Math.max(radius.x, radius.y) * ft.opts.distance
				});
		}

		if ( ft.handles.center ) {
			ft.handles.center.disc.toFront().attr({
				cx: ft.attrs.center.x + ft.attrs.translate.x+ft.attrs.size.x/2+5,
				cy: ft.attrs.center.y + ft.attrs.translate.y
				});
		}

		if ( ft.opts.rotate.indexOf('self') >= 0 ) {
			radius = Math.max(
				Math.sqrt(Math.pow(corners[1].x - corners[0].x, 2) + Math.pow(corners[1].y - corners[0].y, 2)),
				Math.sqrt(Math.pow(corners[2].x - corners[1].x, 2) + Math.pow(corners[2].y - corners[1].y, 2))
				) / 2;
		}
	};

	/**
	 * Add handles
	 */
	ft.showHandles = function() {
		ft.hideHandles();
try {
		ft.axes.map(function(axis) {
			ft.handles[axis] = {};

			ft.handles[axis].line = paper
				.path([ 'M', ft.attrs.center.x, ft.attrs.center.y ])
				.attr({
					stroke: ft.opts.attrs.stroke,
					'stroke-dasharray': '- ',
					opacity: .5
					})
				;

			ft.handles[axis].disc = paper
				.circle(ft.attrs.center.x, ft.attrs.center.y, ft.opts.size.axes)
				.attr(ft.opts.attrs)
				;
		});

		if ( ft.opts.draw.indexOf('bbox') >= 0 ) {
			ft.bbox = paper
				.path('')
				.attr({
					stroke: ft.opts.attrs.stroke,
					//'stroke-dasharray': '- ',
					opacity: .8
					})
				;

			ft.handles.bbox = [];

			var i, handle;

			for ( i = ( ft.opts.scale.indexOf('bboxCorners') >= 0 ? 0 : 4 ); i < ( ft.opts.scale.indexOf('bboxSides') === -1 ? 4 : 8 ); i ++ ) {
				handle = {};

				handle.axis     = i % 2 ? 'x' : 'y';
				handle.isCorner = i < 4;

				handle.element = paper
					.rect(ft.attrs.center.x, ft.attrs.center.y, ft.opts.size[handle.isCorner ? 'bboxCorners' : 'bboxSides' ] * 2, ft.opts.size[handle.isCorner ? 'bboxCorners' : 'bboxSides' ] * 2)
					.attr(ft.opts.attrs)
					;

				ft.handles.bbox[i] = handle;
			}
		}

		if ( ft.opts.draw.indexOf('circle') !== -1 ) {
			ft.circle = paper
				.circle(0, 0, 0)
				.attr({
					stroke: ft.opts.attrs.stroke,
					'stroke-dasharray': '- ',
					opacity: .3
					})
				;
		}

		if ( ft.opts.drag.indexOf('center') !== -1 ) {
			ft.handles.center = {};

			ft.handles.center.disc = paper
				.circle(ft.attrs.center.x+ft.attrs.size.x/2+5, ft.attrs.center.y, ft.opts.size.center)
				.attr(ft.opts.attrs)
				;
		}

		// Drag x, y handles
		ft.axes.map(function(axis) {
			if ( !ft.handles[axis] ) { return; }

			var
				rotate = ft.opts.rotate.indexOf('axis' + axis.toUpperCase()) !== -1,
				scale  = ft.opts.scale .indexOf('axis' + axis.toUpperCase()) !== -1
				;

			ft.handles[axis].disc.drag(function(dx, dy) {
				// viewBox might be scaled
				if ( ft.o.viewBoxRatio ) {
					dx *= ft.o.viewBoxRatio.x;
					dy *= ft.o.viewBoxRatio.y;
				}

				var
					cx = dx + ft.handles[axis].disc.ox,
					cy = dy + ft.handles[axis].disc.oy
					;

				var mirrored = {
					x: ft.o.scale.x < 0,
					y: ft.o.scale.y < 0
					};

				if ( rotate ) {
					var rad = Math.atan2(cy - ft.o.center.y - ft.o.translate.y, cx - ft.o.center.x - ft.o.translate.x);

					ft.attrs.rotate = rad * 180 / Math.PI - ( axis === 'y' ? 90 : 0 );

					if ( mirrored[axis] ) { ft.attrs.rotate -= 180; }
				}

				// Keep handle within boundaries
				if ( ft.opts.boundary ) {
					cx = Math.max(Math.min(cx, ft.opts.boundary.x + ft.opts.boundary.width),  ft.opts.boundary.x);
					cy = Math.max(Math.min(cy, ft.opts.boundary.y + ft.opts.boundary.height), ft.opts.boundary.y);
				}

				var radius = Math.sqrt(Math.pow(cx - ft.o.center.x - ft.o.translate.x, 2) + Math.pow(cy - ft.o.center.y - ft.o.translate.y, 2));

				if ( scale ) {
					ft.attrs.scale[axis] = radius / ( ft.o.size[axis] / 2 * ft.opts.distance );

					if ( mirrored[axis] ) { ft.attrs.scale[axis] *= -1; }
				}

				// Maintain aspect ratio
				if ( ft.opts.keepRatio.indexOf('axis' + axis.toUpperCase()) !== -1 ) {
					keepRatio(axis);
				} else {
					ft.attrs.ratio = ft.attrs.scale.x / ft.attrs.scale.y;
				}

				if ( ft.attrs.scale.x && ft.attrs.scale.y ) { ft.apply(); }

				asyncCallback([ rotate ? 'rotate' : null, scale ? 'scale' : null ]);
			}, function() {
				// Offset values
				ft.o = cloneObj(ft.attrs);

				if ( paper._viewBox ) {
					ft.o.viewBoxRatio = {
						x: paper._viewBox[2] / paper.width,
						y: paper._viewBox[3] / paper.height
						};
				}

				ft.handles[axis].disc.ox = this.attrs.cx;
				ft.handles[axis].disc.oy = this.attrs.cy;

				asyncCallback([ rotate ? 'rotate start' : null, scale ? 'scale start' : null ]);
			}, function() {
                ft.host.editState = true;
                ft.updateHandles();
				asyncCallback([ rotate ? 'rotate end'   : null, scale ? 'scale end'   : null ]);
			});
		});

		// Drag bbox corner handles
		if ( ft.opts.draw.indexOf('bbox') >= 0 && ( ft.opts.scale.indexOf('bboxCorners') !== -1 || ft.opts.scale.indexOf('bboxSides') !== -1 ) ) {
			ft.handles.bbox.map(function(handle) {
				handle.element.drag(function(dx, dy) {
					// viewBox might be scaled
					if ( ft.o.viewBoxRatio ) {
						dx *= ft.o.viewBoxRatio.x;
						dy *= ft.o.viewBoxRatio.y;
					}

					// Maintain aspect ratio
					if ( handle.isCorner && ft.opts.keepRatio.indexOf('bboxCorners') !== -1 ) {
						dx = ( handle.axis === 'x' ? -dy : dy ) * ( ( ft.attrs.size.x * ft.attrs.scale.x ) / ( ft.attrs.size.y * ft.attrs.scale.y ) );
					}

					var sin, cos, rx, ry, rdx, rdy, mx, my, sx, sy;

					/*sin = ft.o.rotate.sin;
					cos = ft.o.rotate.cos;

					// First rotate dx, dy to element alignment
					rx = dx * cos - dy * sin;
					ry = dx * sin + dy * cos;

					rx *= Math.abs(handle.x);
					ry *= Math.abs(handle.y);

					// And finally rotate back to canvas alignment
					rdx = rx *   cos + ry * sin;
					rdy = rx * - sin + ry * cos;*/

                    this.ox = dx / 2;
                    this.oy = dy / 2;

					ft.attrs.translate = {
						x: ft.o.translate.x + this.ox,
						y: ft.o.translate.y + this.oy
					};

					// Mouse position, relative to element center after translation
					mx = ft.o.handlePos.cx + dx - ft.attrs.center.x - ft.attrs.translate.x;
					my = ft.o.handlePos.cy + dy - ft.attrs.center.y - ft.attrs.translate.y;

					// Position rotated to align with element
					/*rx = mx * cos - my * sin;
					ry = mx * sin + my * cos;*/

					// Scale element so that handle is at mouse position
					sx = mx * 2 * handle.x / ft.o.size.x;
					sy = my * 2 * handle.y / ft.o.size.y;

					ft.attrs.scale = {
						x: sx || ft.attrs.scale.x,
						y: sy || ft.attrs.scale.y
					};

					// Maintain aspect ratio
					if ( !handle.isCorner && ft.opts.keepRatio.indexOf('bboxSides') !== -1 ) {
						keepRatio(handle.axis);
					}

					ft.attrs.ratio = ft.attrs.scale.x / ft.attrs.scale.y;

                    if (!ft.copyGhost) {
                        ft.copyGhost = subject.clone().attr({opacity: 0.5, "stroke-dasharray": "-"});
                    }
                    ft.applyGhost(ft.attrs.translate.x, ft.attrs.translate.y);

					//asyncCallback([ 'scale' ]);
				}, function() {
                    this.ox = 0;
                    this.oy = 0;
					var
						rotate = ( ( 360 - ft.attrs.rotate ) % 360 ) / 180 * Math.PI,
						handlePos = handle.element.attr(['x', 'y']);

					// Offset values
					ft.o = cloneObj(ft.attrs);

					ft.o.handlePos = {
						cx: handlePos.x + ft.opts.size[handle.isCorner ? 'bboxCorners' : 'bboxSides'],
						cy: handlePos.y + ft.opts.size[handle.isCorner ? 'bboxCorners' : 'bboxSides']
					};

					// Pre-compute rotation sin & cos for efficiency
					ft.o.rotate = {
						sin: Math.sin(rotate),
						cos: Math.cos(rotate)
						};

					if ( paper._viewBox ) {
						ft.o.viewBoxRatio = {
							x: paper._viewBox[2] / paper.width,
							y: paper._viewBox[3] / paper.height
						};
					}

					//asyncCallback([ 'scale start' ]);
				}, function() {
                    if (ft.copyGhost) {
                        ft.copyGhost.remove();
                        ft.copyGhost = null;
                    }
                    ft.host.editState = true;
                    ft.apply();
                    ft.host.updatePos(subject, this.ox, this.oy);
                    ft.updateHandles();
					//asyncCallback([ 'scale end'   ]);
				});
			});
		}

		// Drag element and center handle
		var draggables = [];

		if ( ft.opts.drag.indexOf('self')   >= 0 && ft.opts.scale.indexOf('self') === -1 && ft.opts.rotate.indexOf('self') === -1 ) { draggables.push(subject); }
		if ( ft.opts.drag.indexOf('center') >= 0 ) { draggables.push(ft.handles.center.disc); }

		draggables.map(function(draggable) {
			draggable.drag(function(dx, dy) {
				// viewBox might be scaled
				if ( ft.o.viewBoxRatio ) {
					dx *= ft.o.viewBoxRatio.x;
					dy *= ft.o.viewBoxRatio.y;
				}
                this.ox = ft.o.translate.x + dx;
                this.oy = ft.o.translate.y + dy;
                if (!ft.copyGhost) {
                    ft.copyGhost = subject.clone().attr({opacity: 0.5});
                }
                ft.applyGhost(this.ox, this.oy);

				//asyncCallback([ 'drag' ]);
			}, function() {
				// Offset values
				ft.o = cloneObj(ft.attrs);
                this.ox = ft.o.translate.x;
                this.oy = ft.o.translate.y;

				if ( ft.opts.snap.drag ) { ft.o.bbox = subject.getBBox(); }

				// viewBox might be scaled
				if ( paper._viewBox ) {
					ft.o.viewBoxRatio = {
						x: paper._viewBox[2] / paper.width,
						y: paper._viewBox[3] / paper.height
						};
				}

				ft.axes.map(function(axis) {
					if ( ft.handles[axis] ) {
						ft.handles[axis].disc.ox = ft.handles[axis].disc.attrs.cx;
						ft.handles[axis].disc.oy = ft.handles[axis].disc.attrs.cy;
					}
				});

				//asyncCallback([ 'drag start' ]);
			}, function() {
                ft.host.editState = true;
                if (ft.copyGhost) {
                    ft.copyGhost.remove();
                    ft.copyGhost = null;
                }
                if (MT.isNumber(this.ox) && MT.isNumber(this.oy)) {
                    if (ft.host.ctrlKeyDn) {
                        ft.host.doCopyNode(subject, this.ox-ft.o.translate.x, this.oy-ft.o.translate.y);
                    } else {
                        ft.attrs.translate.x = this.ox;
                        ft.attrs.translate.y = this.oy;

                        ft.apply();
                        ft.host.updatePos(subject, this.ox-ft.o.translate.x, this.oy-ft.o.translate.y);

                        ft.updateHandles();
                        //asyncCallback([ 'drag end'   ]);
                    }
                }
			});
		});

		ft.updateHandles();
} catch (e) {window.console && window.console.log(e);}
	};

	/**
	 * Remove handles
	 */
	ft.hideHandles = function() {
		ft.items.map(function(item) {
			item.el.undrag();
		});

		if ( ft.handles.center ) {
			ft.handles.center.disc.remove();

			ft.handles.center = null;
		}

		[ 'x', 'y' ].map(function(axis) {
			if ( ft.handles[axis] ) {
				ft.handles[axis].disc.remove();
				ft.handles[axis].line.remove();

				ft.handles[axis] = null;
			}
		});

		if ( ft.bbox ) {
			ft.bbox.remove();

			ft.bbox = null;

			if ( ft.handles.bbox ) {
				ft.handles.bbox.map(function(handle) {
					handle.element.remove();
				});

				ft.handles.bbox = null;
			}
		}

		if ( ft.circle ) {
			ft.circle.remove();

			ft.circle = null;
		}
	};

	// Override defaults
	ft.setOpts = function(options, callback) {
		ft.callback = typeof callback === 'function' ? callback : false;

		var i, j;

		for ( i in options ) {
			if ( options[i] && options[i].constructor === Object ) {
				for ( j in options[i] ) {
					if ( options[i].hasOwnProperty(j) ) {
						ft.opts[i][j] = options[i][j];
					}
				}
			} else {
				ft.opts[i] = options[i];
			}
		}

		if ( ft.opts.animate   === true ) { ft.opts.animate   = { delay:   700, easing: 'linear' }; }
		if ( ft.opts.drag      === true ) { ft.opts.drag      = [ 'center', 'self' ]; }
		if ( ft.opts.keepRatio === true ) { ft.opts.keepRatio = [ 'bboxCorners', 'bboxSides' ]; }
		if ( ft.opts.rotate    === true ) { ft.opts.rotate    = [ 'axisX', 'axisY' ]; }
		if ( ft.opts.scale     === true ) { ft.opts.scale     = [ 'axisX', 'axisY', 'bboxCorners', 'bboxSides' ]; }

		[ 'drag', 'draw', 'keepRatio', 'rotate', 'scale' ].map(function(option) {
			if ( ft.opts[option] === false ) { ft.opts[option] = []; }
		});

		ft.axes = [];

		if ( ft.opts.rotate.indexOf('axisX') >= 0 || ft.opts.scale.indexOf('axisX') >= 0 ) { ft.axes.push('x'); }
		if ( ft.opts.rotate.indexOf('axisY') >= 0 || ft.opts.scale.indexOf('axisY') >= 0 ) { ft.axes.push('y'); }

        if ( typeof ft.opts.size === 'string' ) {
			ft.opts.size = parseFloat(ft.opts.size);
		}

		if ( !isNaN(ft.opts.size) ) {
			ft.opts.size = {
				axes:        ft.opts.size,
				bboxCorners: ft.opts.size,
				bboxSides:   ft.opts.size,
				center:      ft.opts.size+2
				};
		}

		ft.showHandles();

		asyncCallback([ 'init' ]);
	};

	ft.setOpts(options, callback);

	/**
	 * Apply transformations, optionally update attributes manually
	 */
	ft.apply = function() {
		ft.items.map(function(item, i) {
			// Take offset values into account
			var
				center = {
					x: ft.attrs.center.x + ft.offset.translate.x,
					y: ft.attrs.center.y + ft.offset.translate.y
				},
				rotate    = ft.attrs.rotate - ft.offset.rotate,
				scale     = {
					x: ft.attrs.scale.x / ft.offset.scale.x,
					y: ft.attrs.scale.y / ft.offset.scale.y
				},
				translate = {
					x: ft.attrs.translate.x - ft.offset.translate.x,
					y: ft.attrs.translate.y - ft.offset.translate.y
				};

			if ( ft.opts.animate ) {
				asyncCallback([ 'animate start' ]);

				item.el.animate(
					{ transform: [
						'R', rotate, center.x, center.y,
						'S', scale.x, scale.y, center.x, center.y,
						'T', translate.x, translate.y
						] + ft.items[i].transformString },
					ft.opts.animate.delay,
					ft.opts.animate.easing,
					function() {
						asyncCallback([ 'animate end' ]);

						ft.updateHandles();
					}
				);
			} else {
				item.el.transform([
					'R', rotate, center.x, center.y,
					'S', scale.x, scale.y, center.x, center.y,
					'T', translate.x, translate.y
					] + ft.items[i].transformString);

				//asyncCallback([ 'apply' ]);
			}
		});
	};
    ft.applyGhost = function(tx, ty) {
        if (!ft.copyGhost) return;
        var ghost = ft.copyGhost;
        ( ghost.type === 'set' ? ghost.items : [ghost] ).map(function(item, i) {
            var
                center = {
                    x: ft.attrs.center.x + ft.offset.translate.x,
                    y: ft.attrs.center.y + ft.offset.translate.y
                },
                rotate    = ft.attrs.rotate - ft.offset.rotate,
                scale     = {
                    x: ft.attrs.scale.x / ft.offset.scale.x,
                    y: ft.attrs.scale.y / ft.offset.scale.y
                },
                translate = {
                    x: tx - ft.offset.translate.x,
                    y: ty - ft.offset.translate.y
                };


            item.transform([
                'R', rotate, center.x, center.y,
                'S', scale.x, scale.y, center.x, center.y,
                'T', translate.x, translate.y
                ] + ft.items[i].transformString);
        });
    };

	/**
	 * Clean exit
	 */
	ft.unplug = function() {
		var attrs = ft.attrs;

		ft.hideHandles();

		// Goodbye
		delete subject.freeTransform;

		return attrs;
	};

	// Store attributes for each item
	function scan(subject) {
		( subject.type === 'set' ? subject.items : [ subject ] ).map(function(item) {
			if ( item.type === 'set' ) {
				scan(item);
			} else {
				ft.items.push({
					el: item,
					attrs: {
						rotate:    0,
						scale:     { x: 1, y: 1 },
						translate: { x: 0, y: 0 }
						},
					transformString: item.matrix.toTransformString()
					});
			}
		});
	}

	scan(subject);

	// Get the current transform values for each item
	ft.items.map(function(item, i) {
		if ( item.el._ && item.el._.transform && typeof item.el._.transform === 'object' ) {
			item.el._.transform.map(function(transform) {
				if ( transform[0] ) {
					switch ( transform[0].toUpperCase() ) {
						case 'T':
							ft.items[i].attrs.translate.x += transform[1];
							ft.items[i].attrs.translate.y += transform[2];

							break;
						case 'S':
							ft.items[i].attrs.scale.x *= transform[1];
							ft.items[i].attrs.scale.y *= transform[2];

							break;
						case 'R':
							ft.items[i].attrs.rotate += transform[1];

							break;
					}
				}
			});
		}
	});

	// If subject is not of type set, the first item _is_ the subject
	if ( subject.type !== 'set' ) {
		ft.attrs.rotate    = ft.items[0].attrs.rotate;
		ft.attrs.scale     = ft.items[0].attrs.scale;
		ft.attrs.translate = ft.items[0].attrs.translate;

		ft.items[0].attrs = {
			rotate:    0,
			scale:     { x: 1, y: 1 },
			translate: { x: 0, y: 0 }
			};

		ft.items[0].transformString = '';
	}

	ft.attrs.ratio = ft.attrs.scale.x / ft.attrs.scale.y;

	/**
	 * Get rotated bounding box
	 */
	function getBBox() {
		var rad = {
			x: ( ft.attrs.rotate      ) * Math.PI / 180,
			y: ( ft.attrs.rotate + 90 ) * Math.PI / 180
			};

		var radius = {
			x: ft.attrs.size.x / 2 * ft.attrs.scale.x,
			y: ft.attrs.size.y / 2 * ft.attrs.scale.y
			};

		var
			corners = [],
			signs   = [ { x: -1, y: -1 }, { x: 1, y: -1 }, { x: 1, y: 1 }, { x: -1, y: 1 } ]
			;

		signs.map(function(sign) {
			corners.push({
				x: ( ft.attrs.center.x + ft.attrs.translate.x + sign.x * radius.x * Math.cos(rad.x) ) + sign.y * radius.y * Math.cos(rad.y),
				y: ( ft.attrs.center.y + ft.attrs.translate.y + sign.x * radius.x * Math.sin(rad.x) ) + sign.y * radius.y * Math.sin(rad.y)
				});
		});

		return corners;
	}

	function keepRatio(axis) {
		if ( axis === 'x' ) {
			ft.attrs.scale.y = ft.attrs.scale.x / ft.attrs.ratio;
		} else {
			ft.attrs.scale.x = ft.attrs.scale.y * ft.attrs.ratio;
		}
	}

	/**
	 * Recursive copy of object
	 */
	function cloneObj(obj) {
		var i, clone = {};

		for ( i in obj ) {
			clone[i] = typeof obj[i] === 'object' ? cloneObj(obj[i]) : obj[i];
		}

		return clone;
	}

	var timeout = false;

	/**
	 * Call callback asynchronously for better performance
	 */
	function asyncCallback(e) {
		if ( ft.callback ) {
			// Remove empty values
			var events = [];

			e.map(function(e, i) { if ( e ) { events.push(e); } });

			clearTimeout(timeout);

			setTimeout(function() { if ( ft.callback ) { ft.callback(ft, events); } }, 1);
		}
	}

	ft.updateHandles();

	// Enable method chaining
	return ft;
};

R.fn.lineTrans = function(host, subject, options, callback) {
    	// Enable method chaining
	if ( subject.lineTrans ) { return subject.lineTrans; }

	var paper = this,
		bbox  = subject.getBBox(true);

	var ft = subject.lineTrans = {
		attrs: {
            dt: host.lines[subject.id],
			translate: { x: 0, y: 0 }
		},
		callback: callback,
        ep: ['from', 'to'], // end points
		items: [],
		handles: {from: null, to: null},
        offset: {
			translate: { x: 0, y: 0 }
		},
        subject: subject,
        id: subject.id,
        host: host,
        axis: 'from',
        tmpL: null
	};

	ft.updateHandles = function() {
		var attrs = ft.attrs, axis = ft.axis;

        if (ft.handles[axis]) {
            var cx = attrs.dt[axis.charAt(0) + 'Coor'].x,
                cy = attrs.dt[axis.charAt(0) + 'Coor'].y;

            ft.handles[axis].disc.attr({cx: cx, cy: cy}).toFront();
        }
	};

	ft.showHandles = function() {
		ft.hideHandles();

        var attrs = ft.attrs;

		ft.ep.map(function(axis) {
			ft.handles[axis] = {};
			ft.handles[axis].disc = paper
				.circle(attrs.dt[axis.charAt(0) + 'Coor'].x, attrs.dt[axis.charAt(0) + 'Coor'].y, 5)
				.attr({fill:"#cfe4fa",stroke:"#498cd0"});
            ft.handles[axis].disc.axis = axis;

			ft.handles[axis].disc.drag(function(dx, dy) {
                var fx = 0, fy = 0, tx = 0, ty = 0;
                ft.attrs.translate.x = dx;
				ft.attrs.translate.y = dy;
                if (this.axis == 'from') {
                    fx = ft.attrs.translate.x;
                    fy = ft.attrs.translate.y;
                } else {
                    tx = ft.attrs.translate.x;
                    ty = ft.attrs.translate.y;
                }
                ft.axis = this.axis;
                if (ft.attrs.dt.type === 2) {
                    subject.attr({path: ['M', attrs.dt['fCoor'].x+fx, attrs.dt['fCoor'].y+fy, 'L', attrs.dt['tCoor'].x+tx, attrs.dt['tCoor'].y+ty]});
                } else {
                    if (ft.tmpL) {
                        ft.tmpL.attr({path: ['M', attrs.dt['fCoor'].x+fx, attrs.dt['fCoor'].y+fy, 'L', attrs.dt['tCoor'].x+tx, attrs.dt['tCoor'].y+ty]});
                    } else {
                        ft.tmpL = paper.path('').attr({"stroke-dasharray": ".", opacity:0.5});
                    }
                }
			}, function() {
                ft.o = cloneObj(ft.attrs);
                var line = ft.host.lines[ft.id];
                ft.host.drawLine = true;
                if (line && line.arrPos && line.arrPos != "") {
                    line.arr1 && line.arr1.remove();
                    line.arr2 && line.arr2.remove();
                }
			}, function() {
                ft.host.drawLine = false;
                ft.host.editState = true;
                ft.host.removeLineDot();
                if (ft.tmpL){
                    ft.tmpL.remove();
                    ft.tmpL = null;
                }
                var lineIn = ft.host.lineIn, needCheckDir = true,
                    oldfrom = ft.attrs.dt['from'],
                    oldto = ft.attrs.dt['to'], newfrom = 'x', newto = 'x';
                if (lineIn.id && lineIn.pos && lineIn.pos != 'x') {
                    needCheckDir = false;
                    if (ft.axis == 'from') {
                        ft.attrs.dt['from'] = lineIn.id;
                        newfrom = lineIn.id;
                        ft.attrs.dt['fCoor'] = paper.doLine.getJunctionXY(lineIn.pos, ft.host.nodes[lineIn.id].node.getBBox());
                        ft.attrs.dt['fdir'] = lineIn.pos;
                    } else {
                        ft.attrs.dt['to'] = lineIn.id;
                        newto = lineIn.id;
                        ft.attrs.dt['tCoor'] = paper.doLine.getJunctionXY(lineIn.pos, ft.host.nodes[lineIn.id].node.getBBox());
                        ft.attrs.dt['tdir'] = lineIn.pos;
                    }
                    ft.host.lineIn.pos = 'x';
                } else {
                    if (ft.axis == 'from') {
                        ft.attrs.dt['from'] = 'x';
                        ft.attrs.dt['fCoor'].x += ft.attrs.translate.x;
                        ft.attrs.dt['fCoor'].y += ft.attrs.translate.y;
                    } else {
                        ft.attrs.dt['to'] = 'x';
                        ft.attrs.dt['tCoor'].x += ft.attrs.translate.x;
                        ft.attrs.dt['tCoor'].y += ft.attrs.translate.y;
                    }
                }
                var lines = ft.host.lines[ft.id];
                if (lines.type === 1) {
                    needCheckDir && ft.host.updateLineDirect(ft.id);
                    ft.subject.attr({path: paper.doLine.renderEdge(lines)});

                }
                if (lines && lines.arrPos && lines.arrPos != '') {
                    ft.host.makeArrow(ft.id, ft.subject, (lines.arrPos||'') + '-' +(lines.arrType||""));
                }
                ft.host.updateLineRels(ft.id, newfrom, newto, oldfrom, oldto);
                ft.updateHandles();
			});
		});

		ft.updateHandles();
	};

	ft.hideHandles = function() {
		ft.items.map(function(item) {
			item.el.undrag();
		});

		ft.ep.map(function(axis) {
			if (ft.handles[axis]) {
				ft.handles[axis].disc.remove();

				ft.handles[axis] = null;
			}
		});
	};

	ft.showHandles();

	/**
	 * Apply transformations, optionally update attributes manually
	 */
	ft.apply = function() {
		ft.items.map(function(item, i) {
			var translate = {
					x: ft.attrs.translate.x - ft.offset.translate.x,
					y: ft.attrs.translate.y - ft.offset.translate.y
				};

            item.el.transform([
                'T', translate.x, translate.y
                ] + ft.items[i].transformString);

            //asyncCallback([ 'apply' ]);
		});
	};

	/**
	 * Clean exit
	 */
	ft.unplug = function() {
		var attrs = ft.attrs;

		ft.hideHandles();

		// Goodbye
		delete subject.lineTrans;

		return attrs;
	};

	// Store attributes for each item
	function scan(subject) {
		(subject.type === 'set' ? subject.items : [subject]).map(function(item) {
			if ( item.type === 'set' ) {
				scan(item);
			} else {
				ft.items.push({
					el: item,
					attrs: {
						translate: { x: 0, y: 0 }
					},
					transformString: item.matrix.toTransformString()
				});
			}
		});
	}

	scan(subject);

	function cloneObj(obj) {
		var i, clone = {};

		for ( i in obj ) {
			clone[i] = obj[i];
		}

		return clone;
	}

	var timeout = false;

	/**
	 * Call callback asynchronously for better performance
	 */
	function asyncCallback(e) {
		if ( ft.callback ) {
			// Remove empty values
			var events = [];

			e.map(function(e, i) { if ( e ) { events.push(e); } });

			clearTimeout(timeout);

			setTimeout(function() { if ( ft.callback ) { ft.callback(ft, events); } }, 1);
		}
	}

	ft.updateHandles();

	return ft;
};

})(Raphael);